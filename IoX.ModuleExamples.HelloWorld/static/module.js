(function(baseUrl, modElement) {
  var Module = React.createClass({
    getInitialState: function() {
      return {
        message: '',
        replies: []
      };
    },

    handleMessageChange: function(e) {
      this.setState({ message: e.target.value });
    },
    handleSubmit: function(e) {
      e.preventDefault();
      Suave.EvReact.remoteCallback(
        baseUrl + "chat",
        data => this.setState({ replies: this.state.replies.concat([data]) }),
        true
      )(this.state.message);
      this.setState({ message: '' });
    },

    componentDidMount: function() {
      Suave.EvReact.remoteCallback(baseUrl + "helo")();
    },

    componentWillUnmount: function() {
      Suave.EvReact.remoteCallback(baseUrl + "bye")();
    },

    render: function() {
      return (
        <div>
          <h1>Replies</h1>
          {this.state.replies.map((r, idx) => <div key={idx}>{r}</div>)}
          <form onSubmit={this.handleSubmit}>
            <input
              type="text"
              placeholder="Say something..."
              value={this.state.message}
              onChange={this.handleMessageChange}
            />
            <input type="submit" value="Send" />
          </form>
        </div>
      );
    }
  });

  ReactDOM.render(<Module />, modElement);
})
